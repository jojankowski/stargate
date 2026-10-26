import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AstronautDuty, PersonAstronaut, astronautStatus, isRetiredTitle, statusLabels } from '../api/models';
import { StargateApi, errorMessage } from '../api/stargate-api';

@Component({
  selector: 'app-person-detail',
  imports: [DatePipe],
  templateUrl: './person-detail.html',
  styleUrl: './person-detail.scss',
})
export class PersonDetail {
  private readonly api = inject(StargateApi);

  readonly name = input.required<string>();
  readonly saved = output();

  protected readonly person = signal<PersonAstronaut | null>(null);
  protected readonly duties = signal<AstronautDuty[]>([]); // newest first
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly rank = signal('');
  protected readonly dutyTitle = signal('');
  protected readonly startDate = signal('');
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly statusLabels = statusLabels;
  protected readonly isRetired = isRetiredTitle;

  protected readonly status = computed(() => {
    const person = this.person();
    return person ? astronautStatus(person) : 'none';
  });

  constructor() {
    effect(() => {
      const name = this.name();
      untracked(() => this.load(name));
    });
  }

  protected load(name: string = this.name()): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.saveError.set(null);

    this.api.getPersonWithDuties(name).subscribe({
      next: (result) => {
        // ignore it if they clicked someone else while this was loading
        if (name !== this.name()) return;

        this.person.set(result.person);
        this.duties.set(result.astronautDuties);
        this.loading.set(false);
      },
      error: (error) => {
        if (name !== this.name()) return;

        this.person.set(null);
        this.loadError.set(errorMessage(error));
        this.loading.set(false);
      },
    });
  }

  protected addDuty(event: Event): void {
    event.preventDefault();

    const person = this.person();
    if (!person || this.saving()) return;

    this.saving.set(true);
    this.saveError.set(null);

    this.api
      .addDuty({ name: person.name, rank: this.rank(), dutyTitle: this.dutyTitle(), dutyStartDate: this.startDate() })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.rank.set('');
          this.dutyTitle.set('');
          this.startDate.set('');
          this.saved.emit();
          this.load();
        },
        error: (error) => {
          this.saveError.set(errorMessage(error));
          this.saving.set(false);
        },
      });
  }
}
