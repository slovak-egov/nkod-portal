import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useSearchParams } from 'react-router-dom';
import { useUserActivate } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import Loading from '../components/Loading';
import SuccessErrorPage from './SuccessErrorPage';

export default function ActivateUser() {
    const navigate = useNavigate();
    const { t } = useTranslation();
    const [searchParams] = useSearchParams();
    const [activatingUser, error, activate] = useUserActivate();

    useEffect(() => {
        activate({
            id: searchParams.get('id'),
            token: searchParams.get('token')
        });
    }, []);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('header.registrationConfirmation') }]} />
            {activatingUser ? (
                <Loading />
            ) : (
                <SuccessErrorPage
                    msg={error?.message ?? t('activationSuccessful')}
                    isSuccess={!error}
                    backButtonLabel={t('common.backToMain')}
                    backButtonClick={() => navigate('/')}
                />
            )}
        </>
    );
}
