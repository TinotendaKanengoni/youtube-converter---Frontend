import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import path from 'path';
import { HomeComponent } from './home/home.component';
import { ConvertToMp4Component } from './convert-to-mp4/convert-to-mp4.component';

const routes: Routes = [{path:'', pathMatch:'full',redirectTo:'home'},

  {path:'home',component:HomeComponent},
  {path:'convert-to-mp4',component:ConvertToMp4Component}

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }