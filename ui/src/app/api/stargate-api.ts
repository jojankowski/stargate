import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { NewDuty, PersonAstronaut, PersonWithDuties } from './models';

// /api gets proxied to the .NET API (see proxy.conf.json)
@Injectable({ providedIn: 'root' })
export class StargateApi {
  private readonly http = inject(HttpClient);

  getPeople(): Observable<PersonAstronaut[]> {
    return this.http
      .get<{ people: PersonAstronaut[] }>('/api/Person')
      .pipe(map((response) => response.people));
  }

  getPersonWithDuties(name: string): Observable<PersonWithDuties> {
    return this.http.get<PersonWithDuties>(`/api/AstronautDuty/${encodeURIComponent(name)}`);
  }

  // API wants the name as a plain JSON string
  createPerson(name: string): Observable<unknown> {
    return this.http.post('/api/Person', JSON.stringify(name), {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
    });
  }

  addDuty(duty: NewDuty): Observable<unknown> {
    return this.http.post('/api/AstronautDuty', duty);
  }
}

export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    // the dev proxy gives back 502-504 when the API is down
    if ([0, 502, 503, 504].includes(error.status)) {
      return "Can't reach the Stargate API. Is it running?";
    }

    if (error.error?.message) return error.error.message;
  }

  return 'Something went wrong, please try again.';
}
