import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private apiUrl = `${environment.apiUrl}/Home`;
    private tokenKey = 'token';
    private roleKey = 'roleId';
    private roleType = 'roleType';
    private nameKey = 'name';

     private roleTypeSubject = new BehaviorSubject<string>(
        localStorage.getItem(this.roleType) || ''
    );
    roleType$ = this.roleTypeSubject.asObservable();

    constructor(private http: HttpClient, private router: Router) { }

    login(credentials: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/signIn`, credentials).pipe(
            tap((response: any) => {
                if (response && response.token) {
                    localStorage.setItem(this.tokenKey, response.token);
                    // Assuming response.user.roleId exists as per original code
                    if (response.user) {
                        if (response.user.roleId != undefined) {
                            localStorage.setItem(this.roleKey, response.user.roleId.toString());
                        }
                        if (response.user.roleType != undefined) {
                            localStorage.setItem(this.roleType, response.user.roleType.toString());
                        }
                        if (response.user.name) {
                            localStorage.setItem(this.nameKey, response.user.name);
                        }
                    }
                }
            })
        );
    }

    register(userData: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/add`, userData);
    }

    logout(): void {
        localStorage.removeItem(this.tokenKey);
        localStorage.removeItem(this.roleKey);
        localStorage.removeItem(this.nameKey);
        localStorage.removeItem(this.roleType);
        this.router.navigate(['/']);
         this.roleTypeSubject.next('');
    }

    isLoggedIn(): boolean {
        return !!localStorage.getItem(this.tokenKey);
    }

    getToken(): string | null {
        return localStorage.getItem(this.tokenKey);
    }

    getRole(): string | null {
        return localStorage.getItem(this.roleKey);
    }

    getRoleType(): string | null {
        return localStorage.getItem(this.roleType);
    }

    getName(): string | null {
        return localStorage.getItem(this.nameKey);
    }
}