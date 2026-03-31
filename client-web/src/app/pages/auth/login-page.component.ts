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
      <section class="login-hero">
        <p class="eyebrow">Praxis V3</p>
        <h1>Operacoes desenhadas para decisao rapida.</h1>
        <p class="hero-copy">
          Comercial, compras, estoque, faturamento, governanca e observabilidade em uma
          unica superficie operacional.
        </p>

        <div class="hero-grid">
          <article class="hero-card">
            <strong>Multi-filial</strong>
            <p>Contexto por filial, centro de custo e workflow de aprovacao.</p>
          </article>
          <article class="hero-card">
            <strong>Realtime</strong>
            <p>RabbitMQ + SignalR para alertas, inbox operacional e feed ao vivo.</p>
          </article>
          <article class="hero-card">
            <strong>Docker-first</strong>
            <p>Ambiente inteiro pronto para demo, portfolio e operacao local.</p>
          </article>
        </div>
      </section>

      <section class="login-form panel">
        <div class="section-title compact">
          <div>
            <p class="eyebrow">Access</p>
            <h3>Entrar no workspace</h3>
          </div>
        </div>

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
            {{ loading() ? 'Autenticando...' : 'Entrar no Praxis' }}
          </button>
        </form>
      </section>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }

    .login-shell {
      display: grid;
      grid-template-columns: minmax(0, 1.2fr) minmax(360px, 440px);
      min-height: 100vh;
      padding: 2rem;
      gap: 1.5rem;
      align-items: stretch;
    }

    .login-hero,
    .login-form {
      min-height: calc(100vh - 4rem);
    }

    .login-hero {
      padding: 2.5rem;
      border-radius: 32px;
      border: 1px solid rgba(255, 255, 255, 0.08);
      background:
        radial-gradient(circle at top left, rgba(255, 157, 87, 0.26), transparent 32%),
        radial-gradient(circle at 80% 18%, rgba(88, 215, 201, 0.22), transparent 24%),
        linear-gradient(180deg, rgba(10, 21, 31, 0.92), rgba(7, 16, 23, 0.98));
      box-shadow: var(--shadow-xl);
      display: grid;
      align-content: space-between;
      gap: 2rem;
    }

    .login-hero h1 {
      margin: 0.35rem 0 0;
      max-width: 640px;
      font-family: 'Space Grotesk', sans-serif;
      font-size: clamp(3rem, 5vw, 4.8rem);
      line-height: 0.98;
    }

    .hero-copy {
      max-width: 620px;
      margin: 1rem 0 0;
      font-size: 1.08rem;
      line-height: 1.7;
      color: var(--praxis-muted);
    }

    .hero-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 1rem;
    }

    .hero-card {
      padding: 1.1rem;
      border-radius: 22px;
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid rgba(255, 255, 255, 0.06);
    }

    .hero-card p {
      margin: 0.4rem 0 0;
      color: var(--praxis-muted);
      line-height: 1.6;
    }

    .login-form {
      display: grid;
      align-content: center;
      padding: 1.6rem;
    }

    @media (max-width: 980px) {
      .login-shell {
        grid-template-columns: 1fr;
      }

      .login-hero,
      .login-form {
        min-height: auto;
      }

      .hero-grid {
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
