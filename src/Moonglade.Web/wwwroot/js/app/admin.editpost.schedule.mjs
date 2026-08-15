import { getLocalizedString, parseUtcDate, toLocalDateTimeInputValue } from './utils.module.mjs';

export function createScheduleMixin() {
    return {
        enableSchedule: false,
        scheduleInfoHtml: '',
        minScheduleDate: '',

        initScheduleState() {
            if (this.formData.postStatus === 'Scheduled') {
                this.enableSchedule = true;

                if (this.formData.scheduledPublishTimeUtc) {
                    this.formData.scheduledPublishLocalTime = toLocalDateTimeInputValue(
                        this.formData.scheduledPublishTimeUtc);
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
            this.minScheduleDate = toLocalDateTimeInputValue(new Date().toISOString());
        },

        updateScheduleInfo() {
            const status = this.formData.postStatus;

            if (status === 'Scheduled') {
                let displayTime;

                if (this.formData.scheduledPublishLocalTime) {
                    displayTime = new Date(this.formData.scheduledPublishLocalTime).toLocaleString();
                } else if (this.formData.scheduledPublishTimeUtc) {
                    const utcDate = parseUtcDate(this.formData.scheduledPublishTimeUtc);
                    displayTime = utcDate
                        ? utcDate.toLocaleString()
                        : this.formData.scheduledPublishTimeUtc;
                }

                const scheduleText = getLocalizedString('scheduledFor').replace('{0}', displayTime);
                this.scheduleInfoHtml = `<i class="bi-clock"></i> <span>${scheduleText}</span>`;
            } else {
                this.scheduleInfoHtml = '';
            }
        }
    };
}
