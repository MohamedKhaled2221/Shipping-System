
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { DeliveryAgentDto } from '../../../core/models/delivery-agent.models';
import { DeliveryAgentService } from '../../../core/services/delivery-agent.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-agent-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h1>My profile</h1>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else {
      @if (agent(); as a) {
        <div class="layout">
          <form
            class="card"
            [formGroup]="form"
            (ngSubmit)="save()"
          >
            <h3>Contact info</h3>

            <div class="field">
              <label for="name">Name</label>
              <input
                id="name"
                formControlName="name"
              />
            </div>

            <div class="field">
              <label for="phone">Phone</label>
              <input
                id="phone"
                type="tel"
                formControlName="phone"
              />
            </div>

            <button
              type="submit"
              class="btn btn-amber"
              [disabled]="form.invalid || saving()"
            >
              {{ saving() ? 'Saving…' : 'Save changes' }}
            </button>
          </form>

          <div class="card">
            <h3>Availability</h3>

            <p class="hint-text">
              Toggle this off when you're clocking out — it removes you from
              the assignment pool.
            </p>

            <button
              class="btn"
              [class.btn-amber]="!a.isAvailable"
              [class.btn-outline]="a.isAvailable"
              (click)="toggleAvailability(a.isAvailable)"
              [disabled]="togglingAvailability()"
            >
              {{ a.isAvailable ? 'Go unavailable' : 'Go available' }}
            </button>
          </div>
        </div>
      } @else {
        <div class="empty-state">
          Profile not found.
        </div>
      }
    }
  `,
  styles: [
    `
      .layout {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.25rem;
        align-items: start;
        max-width: 720px;
      }

      @media (max-width: 640px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AgentProfileComponent implements OnInit {
  private readonly agentService = inject(DeliveryAgentService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly agent = signal<DeliveryAgentDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly togglingAvailability = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    phone: ['', Validators.required],
  });

  ngOnInit(): void {
    this.agentService.getMyProfile().subscribe({
      next: (agent) => {
        this.agent.set(agent);

        this.form.setValue({
          name: agent.name,
          phone: agent.phone,
        });

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;

    this.saving.set(true);

    this.agentService
      .updateMyProfile(this.form.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (agent) => {
          this.agent.set(agent);
          this.notifications.success('Profile updated.');
        },
      });
  }

  toggleAvailability(currentlyAvailable: boolean): void {
    this.togglingAvailability.set(true);

    this.agentService
      .setMyAvailability({
        isAvailable: !currentlyAvailable,
      })
      .pipe(finalize(() => this.togglingAvailability.set(false)))
      .subscribe({
        next: (agent) => {
          this.agent.set(agent);

          this.notifications.success(
            agent.isAvailable
              ? "You're now available."
              : "You're now unavailable."
          );
        },
      });
  }
}

