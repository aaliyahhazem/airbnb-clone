import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class BaseService {
  protected readonly apiBase = 'http://localhost:5235/api';

  constructor(protected http: HttpClient) {}

  protected buildParams(params: Record<string, any> = {}): HttpParams {
    let p = new HttpParams();
    for (const key of Object.keys(params)) {
      const val = params[key];
      if (val !== undefined && val !== null) p = p.set(key, String(val));
    }
    return p;
  }
}
