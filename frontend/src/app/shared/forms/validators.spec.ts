import { FormControl } from '@angular/forms';
import { httpsUrlValidator, linkedInUrlValidator } from './validators';

const httpsErrors = (value: string | null) => httpsUrlValidator(new FormControl(value));
const linkedInErrors = (value: string | null) => linkedInUrlValidator(new FormControl(value));

describe('httpsUrlValidator', () => {
  it('accepts absolute https URLs', () => {
    expect(httpsErrors('https://example.com/a.png')).toBeNull();
    expect(httpsErrors('  https://example.com  ')).toBeNull();
  });

  it('rejects other schemes and relative URLs', () => {
    for (const value of ['http://example.com/a.png', 'example.com/a.png', 'javascript:alert(1)']) {
      expect(httpsErrors(value)).toEqual({ httpsUrl: true });
    }
  });

  it('treats empty values as valid (optional field)', () => {
    expect(httpsErrors('')).toBeNull();
    expect(httpsErrors(null)).toBeNull();
  });
});

describe('linkedInUrlValidator', () => {
  it('accepts linkedin.com and its subdomains over https', () => {
    expect(linkedInErrors('https://www.linkedin.com/in/octocat')).toBeNull();
    expect(linkedInErrors('https://linkedin.com/in/octocat')).toBeNull();
    expect(linkedInErrors(null)).toBeNull();
  });

  it('rejects http, look-alike hosts and non-URLs', () => {
    for (const value of [
      'http://www.linkedin.com/in/octocat',
      'https://linkedin.com.evil.io/in/x',
      'https://notlinkedin.com/in/x',
      'https://x.io',
      'nonsense',
    ]) {
      expect(linkedInErrors(value)).toEqual({ linkedInUrl: true });
    }
  });
});
