import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Lookup, MarkazLookup, SubProgramLookup, VillageLookup } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class LookupsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/lookups`;

  getPriorities(): Observable<Lookup[]> {
    return this.http.get<Lookup[]>(`${this.base}/priorities`);
  }

  getStatuses(): Observable<Lookup[]> {
    return this.http.get<Lookup[]>(`${this.base}/statuses`);
  }

  getMainPrograms(): Observable<Lookup[]> {
    return this.http.get<Lookup[]>(`${this.base}/main-programs`);
  }

  getSubPrograms(mainProgramId?: number): Observable<SubProgramLookup[]> {
    let params = new HttpParams();
    if (mainProgramId != null) {
      params = params.set('mainProgramId', mainProgramId);
    }
    return this.http.get<SubProgramLookup[]>(`${this.base}/sub-programs`, { params });
  }

  getGovernorates(): Observable<Lookup[]> {
    return this.http.get<Lookup[]>(`${this.base}/governorates`);
  }

  getMarkaz(governorateId?: number): Observable<MarkazLookup[]> {
    let params = new HttpParams();
    if (governorateId != null) {
      params = params.set('governorateId', governorateId);
    }
    return this.http.get<MarkazLookup[]>(`${this.base}/markaz`, { params });
  }

  getVillages(markazId?: number): Observable<VillageLookup[]> {
    let params = new HttpParams();
    if (markazId != null) {
      params = params.set('markazId', markazId);
    }
    return this.http.get<VillageLookup[]>(`${this.base}/villages`, { params });
  }
}
