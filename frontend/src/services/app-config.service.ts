import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' }) 
export class AppConfigService {
  private config: any;

  constructor(private http: HttpClient) {}

  loadConfig() {
    return firstValueFrom(this.http.get('/assets/config.json')).then(data => {
    this.config = data;
  });
  }

  get apiUrl(): string {
    return this.config?.apiUrl || '';
  }
}