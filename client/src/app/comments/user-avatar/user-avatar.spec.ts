import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserAvatar } from './user-avatar';

describe('UserAvatar', () => {
  let fixture: ComponentFixture<UserAvatar>;

  async function avatarOf(name: string): Promise<HTMLElement> {
    fixture = TestBed.createComponent(UserAvatar);
    fixture.componentRef.setInput('name', name);
    await fixture.whenStable();
    return fixture.nativeElement.querySelector('.avatar');
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [UserAvatar] }).compileComponents();
  });

  it('shows the first letter of the name in capitals', async () => {
    const avatar = await avatarOf('sasha1');

    expect(avatar.textContent).toBe('S');
  });

  it('gives the same name the same colour every time', async () => {
    const first = (await avatarOf('Olena2')).style.backgroundColor;
    const second = (await avatarOf('Olena2')).style.backgroundColor;

    expect(first).not.toBe('');
    expect(second).toBe(first);
  });

  it('gives different names different colours', async () => {
    // "a" and "b" differ by one in their letter codes, so they get neighbouring colours
    const first = (await avatarOf('a')).style.backgroundColor;
    const second = (await avatarOf('b')).style.backgroundColor;

    expect(second).not.toBe(first);
  });

  it('is hidden from screen readers, because the name is written next to it', async () => {
    const avatar = await avatarOf('Sasha1');

    expect(avatar.getAttribute('aria-hidden')).toBe('true');
  });

  it('shows a question mark if the name is empty', async () => {
    const avatar = await avatarOf('');

    expect(avatar.textContent).toBe('?');
  });
});
