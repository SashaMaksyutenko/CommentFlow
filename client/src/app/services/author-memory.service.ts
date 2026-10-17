import { Injectable } from '@angular/core';

// Who writes the comment: the fields of the form that stay the same from one comment to the next
export interface AuthorDetails {
  userName: string;
  email: string;
  homePage: string;
}

// Keeps the name, e-mail and home page the visitor typed last time.
// The form is made anew every time the window opens, so it can't keep them itself.
// Only in memory: a page reload forgets them, nothing is saved on the visitor's computer.
@Injectable({ providedIn: 'root' })
export class AuthorMemoryService {
  private details: AuthorDetails = { userName: '', email: '', homePage: '' };

  get(): AuthorDetails {
    return this.details;
  }

  save(details: AuthorDetails): void {
    // A copy, so a later change of the object passed in does not change what is kept
    this.details = { ...details };
  }
}
