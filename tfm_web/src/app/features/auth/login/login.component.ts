import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
    loginForm!: FormGroup;
    isLoading = false;

    constructor(
        private authService: AuthService,
        private router: Router,
        private toastr: ToastrService
    ) { }

    ngOnInit(): void {
        if (this.authService.isLoggedIn()) {
            this.router.navigate(['/dashboard']);
        }

        this.loginForm = new FormGroup({
            email: new FormControl('', [Validators.required, Validators.email]),
            password: new FormControl('', [Validators.required, Validators.minLength(4)])
        });
    }

    showPassword: boolean = false;

        togglePassword() {
        this.showPassword = !this.showPassword;
        }


    submit() {
        if (this.loginForm.invalid) {
            this.loginForm.markAllAsTouched();
            return;
        }

        this.isLoading = true;
        const credentials = {
            Email: this.loginForm.value.email,
            Password: this.loginForm.value.password
        };

        this.authService.login(credentials).subscribe({
            next: (response) => {
                console.log(response);
                this.toastr.success('Login successful');
                this.isLoading = false;
                // Navigation could be done here or in AuthService if centralized
                this.router.navigate(['/dashboard']);
            },
            error: (err) => {
                console.error('Login failed:', err);
                this.toastr.error('Invalid email or password');
                this.isLoading = false;
            }
        });
    }
}
