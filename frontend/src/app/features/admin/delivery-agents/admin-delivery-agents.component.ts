import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { DeliveryAgentDto } from '../../../core/models/delivery-agent.models';
import { DeliveryAgentService } from '../../../core/services/delivery-agent.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-admin-delivery-agents',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="header-row">
      <h1>Delivery agents</h1>
      <button class="btn btn-amber" (click)="showForm.set(!showForm())">
        {{ showForm() ? 'Close' : '+ Onboard agent' }}
      </button>
    </div>

    @if (showForm()) {
      <form class="card create-form" [formGroup]="form" (ngSubmit)="register()">
        <div class="field">
          <label for="name">Name</label>
          <input id="name" formControlName="name" />
        </div>
        <div class="field">
          <label for="phone">Phone (used to sign in)</label>
          <input id="phone" type="tel" formControlName="phone" placeholder="+20 100 000 0000" />
        </div>
        <div class="field">
          <label for="password">Temporary password</label>
          <input id="password" type="password" formControlName="password" />
        </div>
        <button type="submit" class="btn btn-amber" [disabled]="form.invalid || registering()">
          {{ registering() ? 'Onboarding…' : 'Onboard agent' }}
        </button>
      </form>
    }

    <p class="hint-text">Showing agents currently available for assignment (FR-6.3's assignment pool).</p>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else if (agents().length === 0) {
      <div class="empty-state">No agents are currently marked available.</div>
    } @else {
      <table class="manifest-table">
        <thead>
          <tr><th>Name</th><th>Phone</th><th>Available since</th></tr>
        </thead>
        <tbody>
          @for (agent of agents(); track agent.id) {
            <tr>
              <td>{{ agent.name }}</td>
              <td class="mono">{{ agent.phone }}</td>
              <td>{{ agent.createdAt | date: 'mediumDate' }}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: [
    `
      .header-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; }
      .create-form { max-width: 380px; margin-bottom: 1.5rem; }
      .mono { font-family: var(--font-mono); font-size: 0.85rem; }
    `,
  ],
})
export class AdminDeliveryAgentsComponent implements OnInit {
  private readonly agentService = inject(DeliveryAgentService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly agents = signal<DeliveryAgentDto[]>([]);
  readonly loading = signal(true);
  readonly showForm = signal(false);
  readonly registering = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    phone: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.agentService.getAvailable().subscribe({
      next: (agents) => {
        this.agents.set(agents);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  register(): void {
    if (this.form.invalid) return;
    this.registering.set(true);
    this.agentService
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.registering.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Agent onboarded.');
          this.form.reset({ name: '', phone: '', password: '' });
          this.showForm.set(false);
          this.load();
        },
      });
  }
}
