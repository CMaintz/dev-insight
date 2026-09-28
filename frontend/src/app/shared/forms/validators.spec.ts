import { FormControl } from '@angular/forms';
import { httpsUrlValidator, isHttpsUrl, isLinkedInUrl, linkedInUrlValidator } from './validators';

describe('url validators', () => {
  it('accepts only absolute https URLs', () => {
    expect(isHttpsUrl('https://example.com/a.png')).toBe(true);
    expect(isHttpsUrl('  https://example.com  ')).toBe(true);
    expect(isHttpsUrl('http://example.com/a.png')).toBe(false);
    expect(isHttpsUrl('example.com/a.png')).toBe(false);
    expect(isHttpsUrl('javascript:alert(1)')).toBe(false);
  });

  it('accepts linkedin.com and its subdomains over https', () => {
    expect(isLinkedInUrl('https://www.linkedin.com/in/octocat')).toBe(true);
    expect(isLinkedInUrl('https://linkedin.com/in/octocat')).toBe(true);
    expect(isLinkedInUrl('http://www.linkedin.com/in/octocat')).toBe(false);
    expect(isLinkedInUrl('https://linkedin.com.evil.io/in/x')).toBe(false);
    expect(isLinkedInUrl('https://notlinkedin.com/in/x')).toBe(false);
    expect(isLinkedInUrl('nonsense')).toBe(false);
  });

  it('treats empty values as valid (optional fields)', () => {
    expect(httpsUrlValidator(new FormControl(''))).toBeNull();
    expect(linkedInUrlValidator(new FormControl<string | null>(null))).toBeNull();
  });

  it('reports errors under their own keys', () => {
    expect(httpsUrlValidator(new FormControl('http://x.io'))).toEqual({ httpsUrl: true });
    expect(linkedInUrlValidator(new FormControl('https://x.io'))).toEqual({ linkedInUrl: true });
    expect(linkedInUrlValidator(new FormControl('https://www.linkedin.com/in/a'))).toBeNull();
  });
});
