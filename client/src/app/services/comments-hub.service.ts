import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { CommentModel } from '../models/comment.model';
import { CommentEventsService } from './comment-events.service';

// Live updates. The server tells every open page about new comments over a WebSocket (SignalR).
// What comes from the server goes into CommentEventsService, so the table and the replies
// react to it the same way as to a comment sent from this page.
@Injectable({ providedIn: 'root' })
export class CommentsHubService {
  private readonly events = inject(CommentEventsService);

  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl(environment.hubUrl)
    // After a lost connection it tries again by itself: at once, then after 2, 10 and 30 seconds
    .withAutomaticReconnect()
    // Only problems go to the browser console, not every message
    .configureLogging(LogLevel.Warning)
    .build();

  constructor() {
    // The name is the one the hub calls on its clients (ICommentsClient on the server).
    // The author of the comment gets it here too: that only makes the table load once more.
    this.connection.on('CommentCreated', (comment: CommentModel) => {
      this.events.notifyOtherComment(comment);
    });
  }

  // Called once when the app starts
  connect(): void {
    this.connection.start().catch(() => {
      // withAutomaticReconnect works only after the first successful start,
      // so the first start is tried again here (for example while the API is still starting)
      setTimeout(() => this.connect(), 5000);
    });
  }
}
