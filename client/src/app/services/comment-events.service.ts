import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { CommentModel } from '../models/comment.model';

// The place where the app says "a comment was created".
// Whoever creates a comment tells it here, whoever shows comments listens.
// This way the form and the table don't have to know about each other.
@Injectable({ providedIn: 'root' })
export class CommentEventsService {
  private readonly createdSubject = new Subject<CommentModel>();

  // For listening only: components subscribe here but cannot push anything in
  readonly created$ = this.createdSubject.asObservable();

  notifyCreated(comment: CommentModel): void {
    this.createdSubject.next(comment);
  }
}
