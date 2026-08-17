import angular from '@angular-devkit/build-angular/application';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [
    angular({
      ngBuildConfig: {},
    }),
  ],
});
