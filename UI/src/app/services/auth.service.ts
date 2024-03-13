import { Injectable } from '@angular/core';
import { Client, UserInfo } from 'generated';
import { Observable, ReplaySubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {

    private userInfoSubject = new ReplaySubject<UserInfo>(1);
    private userInfo: UserInfo | undefined;

    constructor(private client: Client) { 
        this.client.getUserInfo().subscribe({
            next: z => {
                this.userInfo = z;
                this.userInfoSubject.next(z);
            },
            error: err => {
                alert("Error connecting to server. While this could be anything, it's most likely a sql database connection issue")
            }
        });
    }

    getUserInfo(): UserInfo | undefined{
        return this.userInfo;
    }

    getUserInfoAsync(): Observable<UserInfo>{
        return this.userInfoSubject.asObservable();
    }

    setUserInfo(userInfo: UserInfo) {
        this.userInfo = userInfo;
        this.userInfoSubject.next(userInfo);
    }
}