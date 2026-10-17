import { TestBed } from '@angular/core/testing';
import { AuthorDetails, AuthorMemoryService } from './author-memory.service';

describe('AuthorMemoryService', () => {
  let memory: AuthorMemoryService;

  beforeEach(() => {
    memory = TestBed.inject(AuthorMemoryService);
  });

  it('has empty fields at the start', () => {
    expect(memory.get()).toEqual({ userName: '', email: '', homePage: '' });
  });

  it('gives back what was saved', () => {
    memory.save({ userName: 'Sasha1', email: 'sasha@example.com', homePage: 'https://example.com' });

    expect(memory.get()).toEqual({ userName: 'Sasha1', email: 'sasha@example.com', homePage: 'https://example.com' });
  });

  it('keeps its own copy', () => {
    const details: AuthorDetails = { userName: 'Sasha1', email: 'sasha@example.com', homePage: '' };
    memory.save(details);

    details.userName = 'Changed';

    expect(memory.get().userName).toBe('Sasha1');
  });
});
