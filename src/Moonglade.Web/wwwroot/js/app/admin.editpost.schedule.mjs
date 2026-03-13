export function createScheduleMixin() {
    return {
        enableSchedule: false,
        scheduleInfoHtml: '',
        minScheduleDate: '',

        initScheduleState() {
            if (this.formData.postStatus === 'Scheduled') {
                this.enableSchedule = true;

                if (this.formData.scheduledPublishTimeUtc) {
                    const utcDate = new Date(this.formData.scheduledPublishTimeUtc);
                    const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);

                    const pad = n => n < 10 ? '0' + n : n;
                    const year = localDate.getFullYear();
                    const month = pad(localDate.getMonth() + 1);
                    const day = pad(localDate.getDate());
                    const hours = pad(localDate.getHours());
                    const minutes = pad(localDate.getMinutes());
                    this.formData.scheduledPublishTime = `${year}-${month}-${day}T${hours}:${minutes}`;
                }

                this.updateScheduleInfo();
            }
        },

        openPublishModal() {
            this.updateMinScheduleDate();
            const modal = new bootstrap.Modal(document.getElementById('publishModal'));
            modal.show();
        },

        submitPublish() {
            const modal = bootstrap.Modal.getInstance(document.getElementById('publishModal'));
            if (modal) modal.hide();

            this.submitAction = 'publish';
            this.handleSubmit();
        },

        updateMinScheduleDate() {
            this.minScheduleDate = new Date().toISOString().slice(0, 16);
        },

        updateScheduleInfo() {
            const status = this.formData.postStatus;

            if (status === 'Scheduled') {
                let displayTime;

                if (this.formData.scheduledPublishTime) {
                    displayTime = new Date(this.formData.scheduledPublishTime).toLocaleString();
                } else if (this.formData.scheduledPublishTimeUtc) {
                    const utcDate = new Date(this.formData.scheduledPublishTimeUtc);
                    const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);
                    displayTime = localDate.toLocaleString();
                }

                this.scheduleInfoHtml = `<i class="bi-clock"></i> <span>Scheduled for: ${displayTime}</span>`;
            } else {
                this.scheduleInfoHtml = '';
            }
        }
    };
}
