import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CommentModel } from '../models/comment.model';
import { NewComment } from '../models/new-comment.model';
import { CommentsQuery, PagedResponse } from '../models/paging.model';

// All HTTP calls to the comments API live here, components don't use HttpClient directly
@Injectable({ providedIn: 'root' })
export class CommentsApiService {
  private readonly http = inject(HttpClient);
  private readonly commentsUrl = `${environment.apiUrl}/comments`;

  // One page of top-level comments
  getComments(query: CommentsQuery): Observable<PagedResponse<CommentModel>> {
    const params = new HttpParams()
      .set('page', query.page)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    return this.http.get<PagedResponse<CommentModel>>(this.commentsUrl, { params });
  }

  // All replies under a comment as a tree: every reply has its own "replies"
  getReplies(commentId: number): Observable<CommentModel[]> {
    return this.http.get<CommentModel[]>(`${this.commentsUrl}/${commentId}/replies`);
  }

  // Goes as multipart/form-data and not JSON, because the API takes a file in the same request
  createComment(comment: NewComment): Observable<CommentModel> {
    const body = new FormData();
    body.set('userName', comment.userName);
    body.set('email', comment.email);
    body.set('text', comment.text);
    body.set('captchaId', comment.captchaId);
    body.set('captchaAnswer', comment.captchaAnswer);

    // The field is optional: when it is empty, it is simply not sent
    const homePage = comment.homePage.trim();
    if (homePage !== '') {
      body.set('homePage', homePage);
    }

    return this.http.post<CommentModel>(this.commentsUrl, body);
  }
}
