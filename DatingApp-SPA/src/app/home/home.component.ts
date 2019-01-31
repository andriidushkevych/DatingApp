import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  registerMode: Boolean = false;

  constructor(private http: HttpClient) { }

  ngOnInit() { }

  registerModeToggle() {
    this.registerMode = !this.registerMode;
    console.log("registrMode now : " + this.registerMode);  
  }

  cancelRegisterMode(registerMode: boolean) {
    this.registerMode = registerMode;
  }
}
