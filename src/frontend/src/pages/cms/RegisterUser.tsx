import {useTranslation} from "react-i18next";
import {useNavigate} from "react-router";
import Breadcrumbs from "../../components/Breadcrumbs";
import MainContent from "../../components/MainContent";
import PageHeader from "../../components/PageHeader";
import FormElementGroup from "../../components/FormElementGroup";
import BaseInput from "../../components/BaseInput";
import Button from "../../components/Button";
import {useCmsUserAdd} from "../../cms";
import ErrorAlert from "../../components/ErrorAlert";
import Alert from "../../components/Alert";
import GridRow from "../../components/GridRow";
import GridColumn from "../../components/GridColumn";

export default function RegisterUser() {
    const navigate = useNavigate();
    const {t} = useTranslation();
    const [user, setUser, genericError, saving, save] = useCmsUserAdd({
        firstName: '',
        lastName: '',
        email: '',
        password: '',
        passwordConfirm: '',
        userName: ''
    });
    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {
            title: t('odCommunity'),
            link: '/odkomunita'
        }, {title: t('register')}]}/>
        <MainContent>
            <PageHeader>{t('register')}</PageHeader>

            <FormElementGroup label={t('userName')}
                              element={id => <BaseInput id={id} disabled={saving} value={user.user.userName}
                                                        onChange={e => setUser({
                                                            user: {
                                                                ...user.user,
                                                                userName: e.target.value
                                                            }
                                                        })}/>}/>
            <FormElementGroup label={t('emailAddress')}
                              element={id => <BaseInput type={'email'} id={id} disabled={saving}
                                                        value={user.user.email ?? ''}
                                                        onChange={e => setUser({
                                                            user: {
                                                                ...user.user,
                                                                email: e.target.value
                                                            }
                                                        })}/>}/>
            <GridRow>
                <GridColumn widthUnits={1} totalUnits={2}>
                    <FormElementGroup label={t('password')}
                                      element={id => <BaseInput type={'password'} id={id} disabled={saving}
                                                                value={user.password}
                                                                onChange={e => setUser({password: e.target.value})}/>}/>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={2}>
                    <FormElementGroup label={t('passwordConfirm')}
                                      element={id => <BaseInput type={'password'} id={id} disabled={saving}
                                                                value={user.passwordConfirm}
                                                                onChange={e => setUser({passwordConfirm: e.target.value})}/>}/>
                </GridColumn>
            </GridRow>
            {genericError ?
                <Alert type={'warning'}>
                    <ErrorAlert error={genericError}/>
                </Alert>
                : null}
            <Button style={{marginRight: '20px'}} onClick={async () => {
                const result = await save();
                if (result?.success) {
                    navigate('/odkomunita', {state: {info: t('registrationSuccessful')}});
                }
            }}>
                {t('save')}
            </Button>
            <Button buttonType={'secondary'} onClick={async () => {
                navigate('/odkomunita');
            }}>
                {t('cancel')}
            </Button>
        </MainContent>
    </>;
}