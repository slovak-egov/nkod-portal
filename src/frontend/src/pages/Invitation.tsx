import { useTranslation } from 'react-i18next';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { doLogin, sendGet, useDocumentTitle } from '../client';
import { useEffect, useState } from 'react';
import Alert from '../components/Alert';
import Loading from '../components/Loading';
import { AxiosResponse } from 'axios';
import ErrorAlert from '../components/ErrorAlert';
import Button from '../components/Button';

type InvitationInfo = {
    isValid: boolean;
    firstName: string;
    lastName: string;
    expiresAt: string;
    publisher: string;
    role: string;
};

export default function Invitation() {
    const [info, setInfo] = useState<InvitationInfo | null>();
    const [loading, setLoading] = useState(true);
    const { t } = useTranslation();
    const [error, setError] = useState<Error | null>(null);
    useDocumentTitle(t('invitation'));

    let invitationToken = window.location.search;

    if (invitationToken.length > 1) {
        invitationToken = invitationToken.substring(1);
    }

    useEffect(() => {
        async function load() {
            document.cookie = 'invitation=' + encodeURIComponent(invitationToken) + '; path=/';

            setLoading(true);
            try {
                const response: AxiosResponse<InvitationInfo> = await sendGet('validate-inviation', {});
                setInfo(response.data);
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
                setInfo(null);
            } finally {
                setLoading(false);
            }
        }

        load();
    }, [invitationToken]);

    const login = async () => {
        const url = await doLogin({});
        if (url) {
            window.location.href = url;
        }
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }]} />
            <MainContent>
                <PageHeader>{t('invitation')}</PageHeader>
                {loading ? (
                    <Loading />
                ) : error ? (
                    <ErrorAlert error={error} />
                ) : (
                    <>
                        {info ? (
                            <>
                                {info.isValid ? (
                                    <>
                                        <p className="govuk-body">
                                            {t('invitationIsValid')} {t('invitationWasCreatedForPerson')}: {info.firstName} {info.lastName}.{' '}
                                            {t('forRegistrationDoLogin')}
                                        </p>
                                        <p className="govuk-body">
                                            <Button onClick={login}>{t('login')}</Button>
                                        </p>
                                    </>
                                ) : (
                                    <>
                                        <Alert type="warning">{t('invitationIsNotValid')}</Alert>
                                    </>
                                )}
                            </>
                        ) : (
                            <>
                                <Alert type="warning">{t('invitationIsNotValid')}</Alert>
                            </>
                        )}
                    </>
                )}
            </MainContent>
        </>
    );
}
