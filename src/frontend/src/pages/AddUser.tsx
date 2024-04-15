import PageHeader from '../components/PageHeader';
import Button from '../components/Button';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import FormElementGroup from '../components/FormElementGroup';
import BaseInput from '../components/BaseInput';
import { UserSaveResult, useDocumentTitle, useUserAdd, useUserInfo } from '../client';
import MultiRadio from '../components/MultiRadio';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useState } from 'react';
import Invitation from '../components/Invitation';

type Role = {
    id: string;
    name: string;
};

export default function AddUser() {
    const [userInfo] = useUserInfo();
    const [user, setUser, errors, saving, save] = useUserAdd({
        firstName: '',
        lastName: '',
        email: '',
        role: 'Publisher'
    });
    const { t } = useTranslation();
    const navigate = useNavigate();
    const [invitationToken, setInvitationToken] = useState<string | null>(null);

    const roles: Role[] = [
        {
            id: 'Publisher',
            name: t('publisherUser')
        },
        {
            id: 'PublisherAdmin',
            name: t('publisherAdmin')
        }
    ];
    useDocumentTitle(t('newUser'));

    return (
        <>
            {invitationToken ? (
                <div>
                    <Invitation invitationToken={invitationToken} />
                </div>
            ) : (
                <>
                    <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('userList'), link: '/sprava/pouzivatelia' }, { title: t('newUser') }]} />
                    <MainContent>
                        <PageHeader>{t('newUser')}</PageHeader>
                        {userInfo?.publisherView ? (
                            <p className="govuk-body nkod-publisher-name">
                                <span style={{ color: '#2B8CC4', fontWeight: 'bold' }}>{t('publisher')}</span>
                                <br />
                                {userInfo.publisherView.name}
                            </p>
                        ) : null}

                        <FormElementGroup
                            label={t('firstName')}
                            errorMessage={errors['firstName']}
                            element={(id) => (
                                <BaseInput id={id} disabled={saving} value={user.firstName} onChange={(e) => setUser({ firstName: e.target.value })} />
                            )}
                        />
                        <FormElementGroup
                            label={t('lastName')}
                            errorMessage={errors['lastName']}
                            element={(id) => (
                                <BaseInput id={id} disabled={saving} value={user.lastName} onChange={(e) => setUser({ lastName: e.target.value })} />
                            )}
                        />
                        <FormElementGroup
                            label={t('emailAddress')}
                            errorMessage={errors['email']}
                            element={(id) => (
                                <BaseInput id={id} disabled={saving} value={user.email ?? ''} onChange={(e) => setUser({ email: e.target.value })} />
                            )}
                        />

                        <MultiRadio<Role>
                            label={t('role')}
                            inline
                            disabled={saving}
                            options={roles}
                            id="role-selection"
                            getValue={(v) => v.name}
                            renderOption={(v) => v.name}
                            selectedOption={roles.find((o) => o.id === user.role) ?? roles[0]}
                            onChange={(o) => setUser({ role: o.id })}
                        />

                        <Button
                            style={{ marginRight: '20px' }}
                            onClick={async () => {
                                const result = (await save()) as UserSaveResult;
                                if (result?.success) {
                                    if (result.invitationToken) {
                                        setInvitationToken(result.invitationToken);
                                    } else {
                                        navigate('/sprava/pouzivatelia');
                                    }
                                }
                            }}
                        >
                            {t('save')}
                        </Button>
                    </MainContent>
                </>
            )}
        </>
    );
}
