import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { UserService } from 'src/app/core/services/user.service';
import { AuthService } from '../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';


@Component({
  selector: 'app-products',
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent implements OnInit {

  isLoading = false;
  ShowModal = false;
  today: string = '';
  products: any[] = [];
  productForm!: FormGroup;

  constructor(private authService: AuthService, 
    private userService: UserService,
    private toastr: ToastrService, private router: Router) { }

 ngOnInit(): void {
  
    this.today = new Date().toISOString().split('T')[0];
    this.productForm = new FormGroup({
      productName: new FormControl('', [Validators.required, Validators.minLength(3)]),
      price: new FormControl('', Validators.required),
      stockQuantity: new FormControl('', Validators.required),
      description: new FormControl('', Validators.required),
      createdDate: new FormControl('', Validators.required),
      expireDate: new FormControl('', Validators.required),
      isActive: new FormControl('', Validators.required)
    });
     this.loadProduct();
  }

  openModel(){
    this.ShowModal = true;
  }
  closeViewModal(){
    this.ShowModal = false;
  }

  submit(){

     if (this.productForm.invalid) {
    this.productForm.markAllAsTouched();
    this.toastr.error('Please fill all required fields');
    return;
  }

    this.isLoading = true;
    const products ={
       ProductName: this.productForm.value.productName,
       Price: this.productForm.value.price,
       StockQuantity: this.productForm.value.stockQuantity,
       Description: this.productForm.value.description,
       CreatedDate: this.productForm.value.createdDate,
       ExpireDate: this.productForm.value.expireDate,
       IsAction: this.productForm.value.isAction
    }

    this.authService.addProduct(products).subscribe({
      next: (response: any) => {
        console.log('Product added:', response);
        this.toastr.success(response.message);
        this.productForm.reset();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
    
  }

  loadProduct(){
    this.userService.getProductData().subscribe({
      next:(response:any)=>{
        this.products = response.products;
      }
    });
  }

}
