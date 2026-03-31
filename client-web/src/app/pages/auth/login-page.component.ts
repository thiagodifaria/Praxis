import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStore } from '../../core/auth.store';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-shell">
      <div class="login-stage">
        <aside class="login-sidebar">
          <div>
            <h1>Praxis</h1>
            <p class="login-caption">Workspace operacional</p>
          </div>

          <nav class="login-nav">
            <a class="is-active">Dashboard</a>
            <a>Vendas</a>
            <a>Estoque</a>
            <a>Financeiro</a>
            <a>Operacoes</a>
          </nav>

          <div class="login-sidebar__footer">
            <small>Ambiente local</small>
            <strong>Stack pronta para validacao</strong>
          </div>
        </aside>

        <main class="login-main">
          <section class="login-intro">
            <p class="eyebrow">Acesso</p>
            <h2>Entrar no Praxis</h2>
            <p>
              Acompanhe vendas, compras, estoque, faturamento e governanca em um unico workspace.
            </p>
          </section>

          <section class="panel login-form">
            <form class="stack" (ngSubmit)="submit()">
              <label class="field">
                <span>E-mail</span>
                <input type="email" [(ngModel)]="email" name="email" required />
              </label>

              <label class="field">
                <span>Senha</span>
                <input type="password" [(ngModel)]="password" name="password" required />
              </label>

              <p class="empty-state" *ngIf="!error()">
                Credenciais seedadas: <strong>admin&#64;praxis.local</strong> / <strong>Admin&#64;12345</strong>
              </p>
              <p class="danger-copy" *ngIf="error()">{{ error() }}</p>

              <button class="button" type="submit" [disabled]="loading()">
                {{ loading() ? 'Autenticando...' : 'Entrar' }}
              </button>
            </form>
          </section>
        </main>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }

    .login-shell,
    .login-stage {
      min-height: 100vh;
    }

    .login-stage {
      display: grid;
      grid-template-columns: 14rem minmax(0, 1fr);
    }

    .login-sidebar {
      display: flex;
      flex-direction: column;
      gap: 0.9rem;
      padding: 1.2rem 0.85rem;
      background: #1b1f2f;
      color: #ffffff;
    }

    .login-sidebar h1 {
      margin: 0;
      font-size: 1.12rem;
      font-weight: 600;
    }

    .login-caption {
      margin: 0.15rem 0 0;
      color: rgba(255, 255, 255, 0.45);
      font-size: 0.74rem;
    }

    .login-nav {
      display: grid;
      gap: 0.2rem;
    }

    .login-nav a {
      display: block;
      padding: 0.68rem 0.78rem;
      border-radius: 0.55rem;
      color: rgba(255, 255, 255, 0.68);
      font-size: 0.82rem;
      font-weight: 500;
    }

    .login-nav a.is-active {
      background: rgba(255, 255, 255, 0.08);
      color: #ffffff;
    }

    .login-sidebar__footer {
      margin-top: auto;
      padding: 0.78rem;
      border-radius: 0.75rem;
      background: rgba(255, 255, 255, 0.04);
    }

    .login-sidebar__footer small,
    .login-sidebar__footer strong {
      display: block;
    }

    .login-sidebar__footer small {
      color: rgba(255, 255, 255, 0.48);
      font-size: 0.72rem;
    }

    .login-sidebar__footer strong {
      margin-top: 0.2rem;
      font-size: 0.78rem;
      font-weight: 600;
    }

    .login-main {
      display: grid;
      align-content: center;
      gap: 0.85rem;
      padding: 1.75rem;
      background: var(--praxis-bg);
    }

    .login-intro h2 {
      margin: 0.2rem 0 0;
      color: var(--praxis-heading);
      font-size: 1.35rem;
      font-weight: 600;
    }

    .login-intro p:last-child {
      max-width: 36rem;
      color: var(--praxis-muted);
      line-height: 1.65;
    }

    .login-form {
      max-width: 24rem;
    }

    @media (max-width: 900px) {
      .login-stage {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class LoginPageComponent {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal('');

  email = 'admin@praxis.local';
  password = 'Admin@12345';

  constructor() {
    if (this.auth.isAuthenticated()) {
      void this.router.navigate(['/dashboard']);
    }
  }

  async submit(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    try {
      await this.auth.login(this.email, this.password);
      await this.router.navigate(['/dashboard']);
    } catch (error: unknown) {
      const message = (error as { error?: { error?: { message?: string } } })?.error?.error?.message;
      this.error.set(message ?? 'Nao foi possivel autenticar no momento.');
    } finally {
      this.loading.set(false);
    }
  }
}
