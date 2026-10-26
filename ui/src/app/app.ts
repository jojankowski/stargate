import { Component, computed, inject, signal } from '@angular/core';
import { PersonAstronaut, astronautStatus, statusLabels } from './api/models';
import { StargateApi, errorMessage } from './api/stargate-api';
import { PersonDetail } from './person-detail/person-detail';

@Component({
  selector: 'app-root',
  imports: [PersonDetail],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly api = inject(StargateApi);

  protected readonly people = signal<PersonAstronaut[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly search = signal('');
  protected readonly selectedName = signal<string | null>(null);

  protected readonly newName = signal('');
  protected readonly adding = signal(false);
  protected readonly addError = signal<string | null>(null);

  protected readonly statusOf = astronautStatus;
  protected readonly statusLabels = statusLabels;

  protected readonly visiblePeople = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.people()
      .filter((person) => person.name.toLowerCase().includes(term))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  constructor() {
    this.loadPeople();
  }

  protected loadPeople(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.api.getPeople().subscribe({
      next: (people) => {
        this.people.set(people);
        this.loading.set(false);
      },
      error: (error) => {
        this.loadError.set(errorMessage(error));
        this.loading.set(false);
      },
    });
  }

  protected addPerson(event: Event): void {
    event.preventDefault();

    const name = this.newName().trim();
    if (!name || this.adding()) return;

    this.adding.set(true);
    this.addError.set(null);

    this.api.createPerson(name).subscribe({
      next: () => {
        this.adding.set(false);
        this.newName.set('');
        this.loadPeople();
        this.selectedName.set(name);
      },
      error: (error) => {
        this.addError.set(errorMessage(error));
        this.adding.set(false);
      },
    });
  }
}
