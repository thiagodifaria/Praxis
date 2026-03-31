import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { AuthUser, LoginResponse } from './app.models';

const ACCESS_TOKEN_KEY = 'praxis.access-token';
const REFRESH_TOKEN_KEY = 'praxis.refresh-token';
const USER_KEY = 'praxis.user';
const BRANCH_KEY = 'praxis.branch-id';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);

  readonly accessToken = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));
  readonly refreshToken = signal<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));
  readonly user = signal<AuthUser | null>(this.readStoredUser());
  readonly activeBranchId = signal<string | null>(localStorage.getItem(BRANCH_KEY));

  readonly isAuthenticated = computed(() => !!this.accessToken());
  readonly userName = computed(() => this.user()?.fullName ?? 'Guest');

  constructor() {
    effect(() => {
      const token = this.accessToken();
      if (token) {
        localStorage.setItem(ACCESS_TOKEN_KEY, token);
      } else {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
      }
    });

    effect(() => {
      const token = this.refreshToken();
      if (token) {
        localStorage.setItem(REFRESH_TOKEN_KEY, token);
      } else {
        localStorage.removeItem(REFRESH_TOKEN_KEY);
      }
    });

    effect(() => {
      const user = this.user();
      if (user) {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
      } else {
        localStorage.removeItem(USER_KEY);
      }
    });

    effect(() => {
      const branchId = this.activeBranchId();
      if (branchId) {
        localStorage.setItem(BRANCH_KEY, branchId);
      } else {
        localStorage.removeItem(BRANCH_KEY);
      }
    });
  }

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await firstValueFrom(this.api.login({ email, password }));
    this.consumeLogin(response);
    return response;
  }

  consumeLogin(response: LoginResponse): void {
    this.accessToken.set(response.accessToken);
    this.refreshToken.set(response.refreshToken);
    this.user.set(response.user);
  }

  setActiveBranch(branchId: string | null): void {
    this.activeBranchId.set(branchId);
  }

  logout(): void {
    this.accessToken.set(null);
    this.refreshToken.set(null);
    this.user.set(null);
    this.activeBranchId.set(null);
    void this.router.navigate(['/login']);
  }

  private readStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
