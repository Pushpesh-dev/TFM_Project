import { Component,OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.css']
})
export class HeaderComponent  {

    roleType: string = '';

    constructor(public router: Router, public authService: AuthService) { }

//      ngOnInit(): void {
//         this.authService.roleType$.subscribe(role => {
//         this.roleType = role;
//     });   
//   }

    logOut() {
        this.authService.logout();
        this.router.navigate(['/']);
    }
}