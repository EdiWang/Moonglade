import { compareVersionNumbers, handleSettingsSubmit } from './admin.settings.mjs';
import { error } from './toastService.mjs'

document.querySelector('.btn-check-update').addEventListener('click', function () {
    document.querySelector('.spinner-border').style.display = 'block';
    document.querySelector('.alert-has-new-release').style.display = 'none';
    document.querySelector('.alert-no-new-release').style.display = 'none';
    document.querySelector('.btn-get-update').classList.add('disabled');
    document.querySelector('.btn-get-update').setAttribute('href', '#');

    var updateCheckCanvas = new bootstrap.Offcanvas(document.getElementById('updateCheckCanvas'));
    updateCheckCanvas.show();

    fetch('https://api.github.com/repos/EdiWang/Moonglade/releases/latest', {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    }).then(async (resp) => {
        var data = await resp.json();
        document.querySelector('.spinner-border').style.display = 'none';

        document.getElementById('releaseName').value = data.name;
        document.getElementById('releaseTagName').value = data.tag_name;
        document.getElementById('releaseCreatedAt').value = new Date(data.created_at);

        var c = compareVersionNumbers(data.tag_name.replace('v', ''), '@currentVersion');
        var hasNewVersion = c == 1 && !data.prerelease;

        if (hasNewVersion) {
            document.querySelector('.alert-has-new-release').style.display = 'block';
            var btnGetUpdate = document.querySelector('.btn-get-update');
            btnGetUpdate.classList.remove('disabled');
            btnGetUpdate.setAttribute('href', data.html_url);
        } else {
            document.querySelector('.alert-no-new-release').style.display = 'block';
        }
    }).catch(err => {
        error(err);
        console.error(err);
    });
});

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);
