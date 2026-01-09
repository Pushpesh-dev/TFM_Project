import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.css']
})
export class HeaderComponent {

    roleType: string = '';

    constructor(public router: Router, private authService: AuthService) {
        this.roleType = this.authService.getRoleType() || '';
     }

    logOut() {
        this.authService.logout();
    }
}
