﻿using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record TestNotification : INotification;

public class TestNotificationHandler(IBlogNotification blogNotification, IBlogConfig blogConfig) : INotificationHandler<TestNotification>
{
    public async Task Handle(TestNotification notification, CancellationToken ct)
    {
        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await blogNotification.Enqueue(MailMesageTypes.TestMail, dl, EmptyPayload.Default);
    }
}