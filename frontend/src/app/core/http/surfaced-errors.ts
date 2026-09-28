import { Observable, firstValueFrom } from 'rxjs';

export function errorAlreadyShownByInterceptor(): undefined {
  return undefined;
}

export function resultOrNothing<T>(request: Observable<T>): Promise<T | undefined> {
  return firstValueFrom(request).catch(errorAlreadyShownByInterceptor);
}
