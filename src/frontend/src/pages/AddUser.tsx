import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { useDocumentTitle, useUserAdd, useUserInfo } from "../client";
import MultiRadio from "../components/MultiRadio";
import { useTranslation } from "react-i18next";

type Role = {
    id: string;
    name: string;
}

export default function AddUser()
{
    const [userInfo] = useUserInfo();
    const [user, setUser, errors, saving, save] = useUserAdd({
        firstName: '',
        lastName: '',
        email: '',
        role: 'Publisher',
        identificationNumber: '',
    });
    const {t} = useTranslation();

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

    return <>
    <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('userList'), link: '/sprava/pouzivatelia'}, {title: t('newUser')}]} />
            <MainContent>
            <PageHeader>{t('newUser')}</PageHeader>
                {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    <FormElementGroup label={t('firstName')} errorMessage={errors['firstName']} element={id => <BaseInput id={id} disabled={saving} value={user.firstName} onChange={e => setUser({firstName: e.target.value})} />} />
                    <FormElementGroup label={t('lastName')} errorMessage={errors['lastName']} element={id => <BaseInput id={id} disabled={saving} value={user.lastName} onChange={e => setUser({lastName: e.target.value})} />} />
                    <FormElementGroup label={t('emailAddress')} errorMessage={errors['email']} element={id => <BaseInput id={id} disabled={saving} value={user.email ?? ''} onChange={e => setUser({email: e.target.value})} />} />
                    <FormElementGroup label={t('identificationNumber')} errorMessage={errors['identificationNumber']} element={id => <BaseInput id={id} disabled={saving} value={user.identificationNumber} onChange={e => setUser({identificationNumber: e.target.value})} />} />

                    <MultiRadio<Role> label={t('role')} 
                                    inline 
                                    disabled={saving}
                                    options={roles} 
                                    id="role-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={roles.find(o => o.id === user.role) ?? roles[0]} 
                                    onChange={o => setUser({role: o.id})}  />
                    
                    <Button style={{marginRight: '20px'}} onClick={save}>
                        {t('save')} 
                    </Button>
            </MainContent>
        </>;
}