import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { AppConfigService } from './services/app-config.service';

// Factory function to load config
export function loadAppConfig(configService: AppConfigService) {
  return () => configService.loadConfig();
}

bootstrapApplication(AppComponent, {
  ...appConfig,
  providers: [
    ...appConfig.providers,
    provideHttpClient(),
    provideAnimations(),
    AppConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: loadAppConfig,
      deps: [AppConfigService],
      multi: true
    }
  ]
}).catch((err) => console.error(err));
