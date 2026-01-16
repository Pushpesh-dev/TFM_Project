import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class UserService {
    private apiUrl = `${environment.apiUrl}/Home`;

    constructor(private http: HttpClient) { }

    getUsers(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/getUsers`);
    }

    updateUser(id: number, data: any): Observable<any> {
        return this.http.put(`${this.apiUrl}/update/${id}`, data);
    }

    deleteUser(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/delete/${id}`);
    }
    
    getProductData(): Observable<any[]> {
         return this.http.get<any[]>(`${this.apiUrl}/getProduct`);
    }

}
