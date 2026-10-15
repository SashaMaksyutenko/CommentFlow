import { formatDate } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { ReplyTargetService } from '../../services/reply-target.service';
import { ReplyList } from './reply-list';

describe('ReplyList', () => {
  let fixture: ComponentFixture<ReplyList>;
  let element: HTMLElement;

  // A valid reply; a test changes only the fields it cares about
  function reply(changes: Partial<CommentModel> = {}): CommentModel {
    return {
      id: 2,
      parentId: 1,
      userName: 'Olena2',
      email: 'olena@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:30:00Z',
      attachment: null,
      replies: [],
      ...changes,
    };
  }

  async function show(replies: CommentModel[]): Promise<void> {
    fixture.componentRef.setInput('replies', replies);
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReplyList],
      // A file of a reply can be opened in the lightbox, which loads TXT files over HTTP
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ReplyList);
    element = fixture.nativeElement;
  });

  it('shows the name, e-mail, date and text of every reply', async () => {
    await show([
      reply({ id: 2, userName: 'Olena2', email: 'olena@example.com', text: 'First' }),
      reply({ id: 3, userName: 'Ivan3', email: 'ivan@example.com', text: 'Second' }),
    ]);

    const replies = element.querySelectorAll('.reply');
    expect(replies.length).toBe(2);
    expect(replies[0].textContent).toContain('Olena2');
    expect(replies[0].textContent).toContain('olena@example.com');
    expect(replies[0].textContent).toContain(formatDate('2026-10-01T10:30:00Z', 'dd.MM.yy HH:mm', 'en-US'));
    expect(replies[0].querySelector('.text')?.textContent).toBe('First');
    expect(replies[1].textContent).toContain('Ivan3');
  });

  it('puts an avatar with the first letter in the header of every reply', async () => {
    await show([reply({ id: 2, userName: 'Olena2' }), reply({ id: 3, userName: 'ivan3' })]);

    const avatars = element.querySelectorAll('.reply header app-user-avatar');
    expect(Array.from(avatars).map((avatar) => avatar.textContent?.trim())).toEqual(['O', 'I']);
  });

  it('puts the replies of a reply inside it, at any depth', async () => {
    const third = reply({ id: 4, parentId: 3, text: 'Third level' });
    const second = reply({ id: 3, parentId: 2, text: 'Second level', replies: [third] });
    const first = reply({ id: 2, parentId: 1, text: 'First level', replies: [second] });

    await show([first]);

    // One list per level, each one inside the item of its parent
    const secondLevel = element.querySelector('ul > li > app-reply-list')!;
    const thirdLevel = secondLevel.querySelector('ul > li > app-reply-list')!;
    expect(secondLevel.querySelector('.text')?.textContent).toBe('Second level');
    expect(thirdLevel.querySelector('.text')?.textContent).toBe('Third level');
    expect(thirdLevel.querySelector('app-reply-list')).toBeNull();
  });

  it('draws no nested list for a reply without replies', async () => {
    await show([reply({ replies: [] })]);

    expect(element.querySelector('app-reply-list')).toBeNull();
  });

  it('lets the visitor answer any reply, a nested one too', async () => {
    const nested = reply({ id: 3, parentId: 2, text: 'Nested' });
    await show([reply({ id: 2, replies: [nested] })]);

    // The buttons go in the order of the replies on the screen: the first reply, then the nested one
    element.querySelectorAll<HTMLButtonElement>('.reply-button')[1].click();

    expect(TestBed.inject(ReplyTargetService).target()).toEqual(nested);
  });

  it('shows the file of a reply under its text', async () => {
    const attachment: CommentModel['attachment'] = {
      url: '/uploads/def.txt',
      fileName: 'notes.txt',
      type: 'Text',
      status: 'Ready',
      contentType: 'text/plain',
      size: 37,
    };

    await show([reply({ id: 2, attachment }), reply({ id: 3, attachment: null })]);

    const replies = element.querySelectorAll('.reply');
    expect(replies[0].querySelector('app-attachment-view .text-file')?.textContent).toBe('notes.txt');
    expect(replies[1].querySelector('app-attachment-view')).toBeNull();
  });

  it('makes the user name a link only when there is a home page', async () => {
    await show([reply({ id: 2, homePage: 'https://example.com/' }), reply({ id: 3, homePage: null })]);

    const replies = element.querySelectorAll('.reply');
    expect(replies[0].querySelector('a.name')?.getAttribute('href')).toBe('https://example.com/');
    expect(replies[1].querySelector('a')).toBeNull();
  });

  it('shows the allowed tags of the text and never lets scripts in', async () => {
    const text = 'Hi <strong>all</strong> <script>alert(1)</script><img src="x" onerror="alert(1)">';

    await show([reply({ text })]);

    const block = element.querySelector('.text')!;
    expect(block.querySelector('strong')?.textContent).toBe('all');
    expect(block.querySelector('script')).toBeNull();
    expect(block.querySelector('[onerror]')).toBeNull();
  });
});
