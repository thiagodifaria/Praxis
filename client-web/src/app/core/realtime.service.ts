import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { ApiService } from './api.service';
import { AuthStore } from './auth.store';
import { LiveNotificationMessage } from './app.models';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);
  private connection: HubConnection | null = null;

  readonly connected = signal(false);
  readonly liveMessages = signal<LiveNotificationMessage[]>([]);

  async ensureConnected(): Promise<void> {
    const token = this.auth.accessToken();
    if (!token) {
      return;
    }

    if (this.connection && (this.connection.state === HubConnectionState.Connected || this.connection.state === HubConnectionState.Connecting)) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.api.hubUrl, {
        accessTokenFactory: () => this.auth.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('notification', (message: LiveNotificationMessage) => {
      this.liveMessages.update((current) => [message, ...current].slice(0, 24));
    });

    this.connection.onreconnected(() => {
      this.connected.set(true);
    });

    this.connection.onclose(() => {
      this.connected.set(false);
    });

    await this.connection.start();
    this.connected.set(true);
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.stop();
    this.connection = null;
    this.connected.set(false);
  }

  clearFeed(): void {
    this.liveMessages.set([]);
  }
}
