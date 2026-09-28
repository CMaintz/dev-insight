import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

function parseUrl(value: string): URL | null {
  try {
    return new URL(value);
  } catch {
    return null;
  }
}

function isHttpsUrl(value: string): boolean {
  const url = parseUrl(value.trim());
  return url !== null && url.protocol === 'https:' && url.hostname.length > 0;
}

function isLinkedInUrl(value: string): boolean {
  const url = parseUrl(value.trim());
  if (!url || url.protocol !== 'https:') {
    return false;
  }
  const host = url.hostname.toLowerCase();
  return host === 'linkedin.com' || host.endsWith('.linkedin.com');
}

function optionalUrlValidator(key: string, test: (value: string) => boolean): ValidatorFn {
  return (control: AbstractControl<string | null>): ValidationErrors | null => {
    const value = control.value?.trim();
    if (!value) {
      return null;
    }
    return test(value) ? null : { [key]: true };
  };
}

export const httpsUrlValidator: ValidatorFn = optionalUrlValidator('httpsUrl', isHttpsUrl);

export const linkedInUrlValidator: ValidatorFn = optionalUrlValidator('linkedInUrl', isLinkedInUrl);
