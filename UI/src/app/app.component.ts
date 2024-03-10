import { Component } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { Client } from 'generated';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
    constructor(private router: Router, private client: Client) {
        this.client.getUserInfo().subscribe()
    }
}
