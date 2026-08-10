import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { AuthenticatedUser, TokenApiResponse } from '../api/api.types';
import { TokenStorage } from './token-storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorage);

  private readonly _token = signal<string | null>(null);
  private readonly _user = signal<AuthenticatedUser | null>(null);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly isSuperadmin = computed(() => this._user()?.role === 'Superadmin');

  constructor() {
    const session = this.storage.load();
    if (session) {
      this._token.set(session.token);
      this._user.set(session.user);
    }
  }

  get token(): string | null {
    return this._token();
  }

  async login(identifier: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<TokenApiResponse>('/api/v1/auth/login', { identifier, password }),
    );
    this.setSession(response);
  }

  logout(): void {
    this._token.set(null);
    this._user.set(null);
    this.storage.clear();
  }

  /** Rehydrates the current user from the server, e.g. after a hard reload. */
  async refreshUser(): Promise<void> {
    if (!this._token()) {
      return;
    }

    const user = await firstValueFrom(this.http.get<AuthenticatedUser>('/api/v1/auth/me'));
    this._user.set(user);

    const session = this.storage.load();
    if (session) {
      this.storage.save({ ...session, user });
    }
  }

  private setSession(response: TokenApiResponse): void {
    if (!response.accessToken || !response.user) {
      throw new Error('Login response is missing a token or user.');
    }

    this._token.set(response.accessToken);
    this._user.set(response.user);
    this.storage.save({
      token: response.accessToken,
      expiresAtUtc: response.expiresAtUtc ?? '',
      user: response.user,
    });
  }
}
