import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CommentModel } from '../models/comment.model';
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
}
