import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { UserService } from 'src/app/core/services/user.service';
import { AuthService } from 'src/app/core/services/auth.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

    users: any[] = [];  
    allUsers: any[] = []; 
    searchText: string = '';

    currentPage:number=1;
    ItemPerPage:number=5;

    isLoading = false;
    roleId: number = 0;
    roleType: string = '';
    userName: string = '';

    // Modal State
    isViewModalOpen = false;
    isModalOpen = false;
    selectedUser: any = {};
    selectedViewUser:any = {};

    get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
    }   

    goToPage(page: number) {
    this.currentPage = page;
    }

    isEditDisabled(user: any): boolean {
    return user.roleId === 1;
    }
    isDeleteDisabled(user:any): boolean{
        return user.roleId ===1;
    }

    constructor(
        private userService: UserService,
        private authService: AuthService,
        private toastr: ToastrService,
        private router: Router
    ) { }

    ngOnInit(): void {
        const role = this.authService.getRole();
        this.roleId = role ? Number(role) : 0;
        this.roleType = this.authService.getRoleType() || '';
        this.userName = this.authService.getName() || '';
        this.loadUsers();
    }


    // pagination
    getPaginatedUsers(){
        const startIndex = (this.currentPage - 1)* this.ItemPerPage;
        const endIndex = startIndex + this.ItemPerPage;

        return this.users.slice(startIndex, endIndex);
    }

    get totalPages():number{
        return Math.ceil(this.users.length/this.ItemPerPage);
    }

    nextPage(){
        if(this.currentPage < this.totalPages){
            this.currentPage++;
        }
    }

    prePage(){
        if(this.currentPage > 1){
            this.currentPage--;
        }
    }

    // load users
    loadUsers() {
        this.isLoading = true;
        this.userService.getUsers().subscribe({
            next: (response:any) => {
                this.allUsers = response.data;
                this.users = response.data;
                this.isLoading = false;
            },
            error: (err) => {
                console.error(err);
                this.toastr.error('Failed to load users');
                this.isLoading = false;
            }
        });
    }


    // search
    search(event: Event){
        this.currentPage = 1;
        const value = (event.target as HTMLInputElement).value;

         this.router.navigate([], {
            queryParams: { search: value },
            queryParamsHandling: 'merge'
        });

        this.users = this.allUsers.filter(user=>
            user.name.toLowerCase().includes(value.toLowerCase()) 
        );
    }

    // view Model
    openViewModal(user:any){
        this.selectedViewUser = {
        id: user.id,
        name: user.name,
        email: user.email,
        department: user.department,
        roleType: user.roleId === 1 ? 'Admin' : 'User'
        };
      this.isViewModalOpen = true;
    }

    closeViewModal(){
       this.isViewModalOpen = false;
    }
     
    openEditModal(user: any){
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
