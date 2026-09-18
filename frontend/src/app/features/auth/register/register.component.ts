import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card card">
      <h1>Create a customer account</h1>
      <p class="hint-text auth-card__lede">
        Admin and delivery-agent accounts are created by an administrator — this form is for customers only.
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="field">
          <label for="name">Full name</label>
          <input id="name" formControlName="name" />
        </div>
        <div class="field">
          <label for="email">Email</label>
          <input id="email" type="email" formControlName="email" />
        </div>
        <div class="field">
          <label for="phone">Phone number</label>
          <input id="phone" type="tel" formControlName="phone" placeholder="+20 100 000 0000" />
        </div>
        <div class="field">
          <label for="password">Password</label>
          <input id="password" type="password" formControlName="password" />
        </div>

        <button type="submit" class="btn btn-amber" [disabled]="form.invalid || submitting()">
          {{ submitting() ? 'Creating account…' : 'Create account' }}
        </button>
      </form>

      <p class="hint-text auth-card__footer">Already registered? <a routerLink="/login">Sign in</a></p>
    </div>
  `,
  styles: [
    `
      .auth-card { max-width: 420px; margin: 2rem auto; }
      .auth-card__lede { margin-top: -0.5rem; margin-bottom: 1.25rem; }
      form button[type='submit'] { width: 100%; margin-top: 0.25rem; }
      .auth-card__footer { text-align: center; margin-top: 1rem; }
    `,
  ],
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.auth
      .registerCustomer(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Account created. Welcome!');
          this.router.navigateByUrl('/products');
        },
      });
  }
}
