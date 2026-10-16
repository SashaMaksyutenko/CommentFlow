import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { AddedNotice, NOTICE_TIME_MS } from './added-notice';

describe('AddedNotice', () => {
  let fixture: ComponentFixture<AddedNotice>;
  let element: HTMLElement;
  let events: CommentEventsService;

  // A valid comment; a test changes only the fields it cares about
  function comment(changes: Partial<CommentModel> = {}): CommentModel {
    return {
      id: 1,
      parentId: null,
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:27:00Z',
      attachment: null,
      replies: [],
      ...changes,
    };
  }

  // The text of the notice, empty when it is not shown
  function notice(): string {
    return element.querySelector('.notice')?.textContent?.trim() ?? '';
  }

  beforeEach(() => {
    // The notice goes away by a timer, so the tests move the time themselves
    vi.useFakeTimers();
    TestBed.configureTestingModule({ imports: [AddedNotice] });
    events = TestBed.inject(CommentEventsService);
    fixture = TestBed.createComponent(AddedNotice);
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => vi.useRealTimers());

  it('shows nothing at first, but the place for the notice is already there', () => {
    expect(notice()).toBe('');
    expect(element.querySelector('[role="status"]')).not.toBeNull();
  });

  it('says that the comment was added after the visitor sends one', () => {
    events.notifyOwnComment(comment());
    fixture.detectChanges();

    expect(element.querySelector('[role="status"]')?.textContent?.trim()).toBe('Your comment was added.');
  });

  it('says "reply" when the visitor has answered a comment', () => {
    events.notifyOwnComment(comment({ id: 2, parentId: 1 }));
    fixture.detectChanges();

    expect(notice()).toBe('Your reply was added.');
  });

  it('keeps quiet about comments of other people', () => {
    events.notifyOtherComment(comment());
    fixture.detectChanges();

    expect(notice()).toBe('');
  });

  it('goes away by itself after a few seconds', () => {
    events.notifyOwnComment(comment());
    fixture.detectChanges();

    vi.advanceTimersByTime(NOTICE_TIME_MS - 1);
    fixture.detectChanges();
    expect(notice()).toBe('Your comment was added.');

    vi.advanceTimersByTime(1);
    fixture.detectChanges();
    expect(notice()).toBe('');
  });

  it('starts the time again when one more comment is sent', () => {
    events.notifyOwnComment(comment());
    vi.advanceTimersByTime(NOTICE_TIME_MS - 1000);

    events.notifyOwnComment(comment({ id: 2, parentId: 1 }));
    vi.advanceTimersByTime(NOTICE_TIME_MS - 1000);
    fixture.detectChanges();

    // The first timer would have ended by now, but the second notice is still on the screen
    expect(notice()).toBe('Your reply was added.');
  });
});
