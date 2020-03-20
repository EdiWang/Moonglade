'use strict';
// Replace dark theme class
if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    // dark mode
    $('.navbar-light').addClass('navbar-dark');
    $('.navbar-light').removeClass('navbar-light');
    $('body').addClass('bg-dark');
    $('body').css('color', 'white');
    $('.modal-content').addClass('bg-dark');
    $('.modal-content').css('color', 'white');
    $('.container-fluid').addClass('bg-dark');
    $('.container-fluid').css('color', 'white');
    $('.list-group-item').addClass('bg-dark');
    $('.list-group-item').css('color', 'white');
    $('.content-wrapper').addClass('bg-dark');
    $('.content-wrapper').css('color', 'white');
    $('.card').addClass('bg-dark');
    $('.bg-light').addClass('bg-dark');
    $('.bg-light').removeClass('bg-light');
    $('.bg-white').addClass('bg-dark');
    $('.bg-white').removeClass('bg-white');
    $('.bd-footer').addClass('bg-dark');
    $('.jumbotron').css('background-color', '#191c20');
    $('.breadcrumb').css('background-color', 'rgb(42, 43, 45)');
    $('pre').css('background-color', '#444');
    $('code').css('background-color', '#444');
    $('pre').css('color', '#fff');
    $('code').css('color', '#fff');
}