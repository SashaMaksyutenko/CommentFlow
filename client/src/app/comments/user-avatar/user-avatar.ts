import { Component, computed, input } from '@angular/core';

// Colours for the circles. All are dark enough for a white letter to be easy to read.
const AVATAR_COLORS = ['#4a67c9', '#2f855a', '#c05621', '#6b46c1', '#b83280', '#2b6cb0', '#975a16', '#2c7a7b'];

// The same name always gives the same number, so one person always has the same colour
function colorIndex(name: string): number {
  let sum = 0;
  for (const char of name) {
    sum += char.charCodeAt(0);
  }
  return sum % AVATAR_COLORS.length;
}

// A round badge with the first letter of the user name, like the avatars in the assignment picture
@Component({
  selector: 'app-user-avatar',
  styleUrl: './user-avatar.css',
  templateUrl: './user-avatar.html',
})
export class UserAvatar {
  readonly name = input.required<string>();

  // The name is never empty on the server, '?' is only a safety net
  protected readonly letter = computed(() => this.name().charAt(0).toUpperCase() || '?');
  protected readonly color = computed(() => AVATAR_COLORS[colorIndex(this.name())]);
}
