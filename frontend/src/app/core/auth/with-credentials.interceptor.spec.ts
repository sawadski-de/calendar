import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { withCredentialsInterceptor } from './with-credentials.interceptor';

describe('withCredentialsInterceptor', () => {
  it('sets withCredentials on every outgoing request', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([withCredentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    const http = TestBed.inject(HttpClient);
    const httpMock = TestBed.inject(HttpTestingController);

    http.get('/api/whatever').subscribe();

    const req = httpMock.expectOne('/api/whatever');
    expect(req.request.withCredentials).toBe(true);
    req.flush(null);
    httpMock.verify();
  });
});
