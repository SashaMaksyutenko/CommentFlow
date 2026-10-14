import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { CommentModel } from '../models/comment.model';

// A created comment, and whether the visitor of this page wrote it
export interface CreatedComment {
  comment: CommentModel;

  // true: sent from the form on this page; false: written by someone else and came with live updates
  own: boolean;
}

// The place where the app says "a comment was created".
// Whoever creates a comment tells it here, whoever shows comments listens.
// This way the form and the table don't have to know about each other.
@Injectable({ providedIn: 'root' })
export class CommentEventsService {
  private readonly createdSubject = new Subject<CreatedComment>();

  // For listening only: components subscribe here but cannot push anything in
  readonly created$ = this.createdSubject.asObservable();

  // The visitor has just sent this comment from the form
  notifyOwnComment(comment: CommentModel): void {
    this.createdSubject.next({ comment, own: true });
  }

  // Someone else has written this comment
  notifyOtherComment(comment: CommentModel): void {
    this.createdSubject.next({ comment, own: false });
  }
}
