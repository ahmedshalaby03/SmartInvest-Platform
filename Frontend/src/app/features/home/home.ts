import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  protected readonly imgFailed = signal(false);
  protected readonly particles = Array.from({ length: 18 }, (_, i) => i);
}
