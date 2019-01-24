import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { NgModel } from '@angular/forms';
import { baseDirectiveCreate } from '@angular/core/src/render3/instructions';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  baseUrl = 'http://localhost:60231/api/auth/';

  constructor(private http: HttpClient) { }

  login(model: any) {
    return this.http.post(this.baseUrl + 'login', model)
      .pipe(
        map((response: any) => {
          const user = response;
          if (user) {
            localStorage.setItem('token', user.token);
          }
        })
      );
  }

  register(model: any) {
    return this.http.post(this.baseUrl + 'register', model);
  }

}
