document.addEventListener("DOMContentLoaded", function () {
    const logoutButton = document.querySelector("#logoutButton");

    if (logoutButton) {
        logoutButton.addEventListener("click", function (event) {
            event.preventDefault();

            fetch('/Identity/Account/Logout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            }).then(response => {
                if (response.ok) {
                    window.location.href = '/';
                } else {
                    alert("Logout failed!");
                }
            }).catch(error => console.error("Error logging out:", error));
        });
    }
});
