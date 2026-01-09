import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { UserService } from 'src/app/core/services/user.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

    users: any[] = [];
    isLoading = false;
    roleId: number = 0;
    roleType: string = '';
    userName: string = '';

    // Modal State
    isModalOpen = false;
    selectedUser: any = {};

    constructor(
        private userService: UserService,
        private authService: AuthService,
        private toastr: ToastrService
    ) { }

    ngOnInit(): void {
        const role = this.authService.getRole();
        this.roleId = role ? Number(role) : 0;
        this.roleType = this.authService.getRoleType() || '';
        this.userName = this.authService.getName() || '';
        this.loadUsers();
    }

    loadUsers() {
        this.isLoading = true;
        this.userService.getUsers().subscribe({
            next: (data) => {
                this.users = data;
                this.isLoading = false;
            },
            error: (err) => {
                console.error(err);
                this.toastr.error('Failed to load users');
                this.isLoading = false;
            }
        });
    }

    openEditModal(user: any) {
        // Clone object to avoid live editing in table
        this.selectedUser = {
            id: user.id,
            name: user.name,
            email: user.email,
            department: user.department
        };
        this.isModalOpen = true;
    }

    closeModal() {
        this.isModalOpen = false;
        this.selectedUser = {};
    }

    updateUser() {
        this.isLoading = true;
        const updateData = {
            Name: this.selectedUser.name,
            Email: this.selectedUser.email,
            Department: this.selectedUser.department
        };

        this.userService.updateUser(this.selectedUser.id, updateData).subscribe({
            next: () => {
                this.toastr.success('User updated successfully');
                this.closeModal();
                this.loadUsers();
            },
            error: () => {
                this.toastr.error('Update failed');
                this.isLoading = false;
            }
        });
    }

    deleteUser(id: number) {
        if (confirm('Are you sure you want to delete this user?')) {
            this.userService.deleteUser(id).subscribe({
                next: () => {
                    this.toastr.success('User deleted');
                    this.loadUsers();
                },
                error: () => {
                    this.toastr.error('Delete failed');
                }
            });
        }
    }
}
