import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

type LoginRole = 'customer' | 'admin' | 'agent';

const ROLE_META: Record<LoginRole, { label: string; identifierLabel: string; identifierType: string }> = {
  customer: { label: 'Customer', identifierLabel: 'Email', identifierType: 'email' },
  admin: { label: 'Admin', identifierLabel: 'Email', identifierType: 'email' },
  agent: { label: 'Delivery agent', identifierLabel: 'Phone number', identifierType: 'tel' },
};

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card card">
      <h1>Sign in</h1>

      <div class="role-switch">
        @for (r of roles; track r) {
          <button
            type="button"
            class="role-switch__option"
            [class.role-switch__option--active]="role() === r"
            (click)="role.set(r)"
          >
            {{ roleMeta[r].label }}
          </button>
        }
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="field">
          <label for="identifier">{{ roleMeta[role()].identifierLabel }}</label>
          <input
            id="identifier"
            [type]="roleMeta[role()].identifierType"
            formControlName="identifier"
            [placeholder]="role() === 'agent' ? '+20 100 000 0000' : 'you@example.com'"
          />
        </div>
        <div class="field">
          <label for="password">Password</label>
          <input id="password" type="password" formControlName="password" />
        </div>

        <button type="submit" class="btn btn-amber" [disabled]="form.invalid || submitting()">
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>

      @if (role() === 'customer') {
        <p class="hint-text auth-card__footer">
          New here? <a routerLink="/register">Create a customer account</a>
        </p>
      } @else {
        <p class="hint-text auth-card__footer">
          {{ role() === 'admin' ? 'Admin' : 'Agent' }} accounts are created by an administrator — there is no
          self-registration for this role.
        </p>
      }
    </div>
  `,
  styles: [
    `
      .auth-card { max-width: 380px; margin: 2rem auto; }
      .role-switch { display: flex; border: 1px solid var(--color-line); border-radius: var(--radius-sm); overflow: hidden; margin-bottom: 1.25rem; }
      .role-switch__option {
        flex: 1;
        background: transparent;
        border: none;
        border-radius: 0;
        color: var(--color-muted);
        font-size: 0.78rem;
        padding: 0.5em 0.3em;
      }
      .role-switch__option:hover { background: rgba(18, 35, 61, 0.05); }
      .role-switch__option--active { background: var(--color-ink); color: #fff; }
      form button[type='submit'] { width: 100%; margin-top: 0.25rem; }
      .auth-card__footer { text-align: center; margin-top: 1rem; }
    `,
  ],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);

  readonly roles: LoginRole[] = ['customer', 'admin', 'agent'];
  readonly roleMeta = ROLE_META;
  readonly role = signal<LoginRole>((this.route.snapshot.queryParamMap.get('role') as LoginRole) ?? 'customer');
  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    identifier: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) return;
    const { identifier, password } = this.form.getRawValue();
    const request = { identifier, password };

    const login$ =
      this.role() === 'admin'
        ? this.auth.loginAdmin(request)
        : this.role() === 'agent'
          ? this.auth.loginAgent(request)
          : this.auth.loginCustomer(request);

    this.submitting.set(true);
    login$.pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => {
        this.notifications.success('Signed in.');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
        this.router.navigateByUrl(returnUrl);
      },
    });
  }
}
