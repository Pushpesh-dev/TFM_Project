import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-register',
    templateUrl: './register.component.html',
    styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {

    registerForm!: FormGroup;
    isLoading = false;
    isLoggedIn = false;
    today: string = new Date().toISOString().split('T')[0];

    constructor(
        private authService: AuthService,
        private router: Router,
        private toastr: ToastrService
    ) { }

    ngOnInit(): void {
        this.isLoggedIn = this.authService.isLoggedIn();

        // Only redirect if user wants to prevent re-login, but here we want Admins to create users
        // So we remove the auto-redirect back to dashboard.


        this.registerForm = new FormGroup({
            fullName: new FormControl('', [Validators.required, Validators.minLength(3)]),
            email: new FormControl('', [
                Validators.required,
                Validators.pattern('^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$')
            ]),
            password: new FormControl('', [
                Validators.required,
                Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{4,}$')
            ]),
            department: new FormControl('', Validators.required),
            joiningDate: new FormControl('', Validators.required),
            roleId: new FormControl('', Validators.required)
        });
    }

      showPassword: boolean = false;

        togglePassword() {
        this.showPassword = !this.showPassword;
        }

        
    submit() {
        if (this.registerForm.invalid) {
            this.registerForm.markAllAsTouched();
            return;
        }

        this.isLoading = true;

        const userData = {
            Name: this.registerForm.value.fullName,
            Email: this.registerForm.value.email,
            Password: this.registerForm.value.password,
            Department: this.registerForm.value.department,
            JoiningDate: this.registerForm.value.joiningDate,
            roleId: this.registerForm.value.roleId
        };

        this.authService.register(userData).subscribe({
            next: () => {
                this.toastr.success('Registration successful');
                if (this.isLoggedIn) {
                    this.router.navigate(['/dashboard']);
                } else {
                    this.router.navigate(['/']);
                }
                this.isLoading = false;
            },
            error: (err) => {
                console.error('Registration failed:', err);
                this.toastr.error('Registration failed. Please try again.');
                
            }
        });
        this.isLoading = false;
    }
}
