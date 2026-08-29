/**
 * Vite configuration for Angular project.
 *
 * The Angular CLI (ng serve / ng build) handles the core build process
 * and automatically swaps environment files based on the build configuration.
 *
 * This file is used for additional Vite customization when needed (plugins,
 * aliases, etc.). Environment swapping is handled by the Angular CLI via
 * the `index.ts` loader in src/environments/.
 *
 * Build commands:
 *   ng serve            → development mode (environment.development.ts)
 *   ng build            → production mode (environment.production.ts)
 *   ng build --configuration development
 *   ng build --configuration production
 */
import { defineConfig } from 'vite';

export default defineConfig({
  // Additional Vite configuration can go here
  // Environment files are swapped automatically by the Angular CLI
});
